const fs = require('fs');
const path = require('path');

const dir = '/app/dist';
const settingsFileName = 'settings.json';
const htmlFileName = 'index.html';

const processChange = (directory) => {

  const settings = fs.readFileSync(path.join(directory, settingsFileName)).toString();
  const htmlContent = fs.readFileSync(path.join(directory, htmlFileName)).toString();

  const newHtmlContent = htmlContent.replace(/\/\*\s*<inject-settings>\s*\*\/[\s\S]*\/\*\s*<\/inject-settings>\s*\*\//g,
    `/*<inject-settings>*/\nwindow.__injected_settings__ =  ${settings}\n/*</inject-settings>*/`);

    if (htmlContent !== newHtmlContent) {
        fs.writeFileSync(path.join(directory, htmlFileName), newHtmlContent);
        console.log(`Processed directory ${directory}.`);
    }
};

try {
  processChange(dir);
} catch (e){
  console.error(e);
}

let watcher;

const createWatcher = () => {

  if(watcher) {
    watcher.close();
  }

  watcher = fs.watch(dir, {encoding: 'buffer'});
  watcher.on('change', (eventType, filename) => {

    const name = filename.toString();

    if (name === settingsFileName || name === htmlFileName) {
      try {
        processChange(dir);
      } catch (e){
        console.error(e);
      }
    }
  });

  try {
    processChange(dir);
  } catch (e){
    console.error(e);
  }
};

try {
  createWatcher();
} catch (e){
  console.error(e);
}

fs.watch(path.dirname(dir), {encoding: 'buffer'}, (eventType, filename) => {
  if(eventType === 'rename' 
    && filename.toString() === path.basename(dir)
    && fs.existsSync(dir)) {
      createWatcher();
  }
});

console.log('Inject-Settings is now running.');
