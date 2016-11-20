import './polyfills.browser.aot';
import './scripts.imports';
import './styles.imports';
import {enableProdMode} from '@angular/core';
import {platformBrowser} from '@angular/platform-browser';
import {AppModuleNgFactory} from './compiled/src/app/app.module.ngfactory';

declare var ENV : string;

if ('production' === ENV) {
    enableProdMode();
}

export function main() {
    return platformBrowser().bootstrapModuleFactory(AppModuleNgFactory)
        .catch(err => console.log(err));
}

export function bootstrapDomReady() {
    document.addEventListener('DOMContentLoaded', main);
}

bootstrapDomReady();
