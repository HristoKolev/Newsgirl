import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import {EntriesComponent} from './entries.component';

@NgModule({
    imports: [BrowserModule],
    declarations: [EntriesComponent],
    exports: [EntriesComponent]
})
export class EntriesModule {
}