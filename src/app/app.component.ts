import {Component} from '@angular/core';
import {Entry} from './data-models';

@Component({
    moduleId: module.id,
    selector: 'app-component',
    templateUrl: 'app.component.html',
    styleUrls: ['app.component.css']
})
export class AppComponent {

    public entries : Entry[];

    constructor() {

        const array : Entry[] = [];

        for (let i = 0; i < 10; i += 1) {

            array.push({
                date: new Date(),
                title: 'Title ' + i,
                url: 'http://google.com'
            });
        }

        this.entries = array;
    }
}