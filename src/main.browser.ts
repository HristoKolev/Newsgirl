import './polyfills.browser';
import './scripts.imports';
import './styles.imports';
import {enableProdMode} from '@angular/core';
import {platformBrowserDynamic} from '@angular/platform-browser-dynamic';
import {AppModule} from './app/app.module';

declare var ENV : string;

if ('production' === ENV) {
    enableProdMode();
}

export function main() {
    return platformBrowserDynamic().bootstrapModule(AppModule)
        .catch(err => console.log(err));
}

export function bootstrapDomReady() {
    document.addEventListener('DOMContentLoaded', main);
}

bootstrapDomReady();

