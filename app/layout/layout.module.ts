import {NgModule} from '@angular/core';
import {BrowserModule} from '@angular/platform-browser';
import {RouterModule} from '@angular/router';
import {SidebarComponent} from './sidebar/sidebar.component';
import {HeaderComponent} from './header/header.component';

@NgModule({
    imports: [
        BrowserModule,
        RouterModule
    ],
    declarations: [
        SidebarComponent,
        HeaderComponent
    ],
    providers: [],
    exports: [
        SidebarComponent,
        HeaderComponent
    ]
})
export class LayoutModule {
}