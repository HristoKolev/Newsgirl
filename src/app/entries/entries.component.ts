import {Component, Input} from '@angular/core';
import {Entry} from '../data-models';

@Component({
    moduleId: module.id,
    selector: 'entries-component',
    templateUrl: 'entries.component.html',
    styleUrls: ['entries.component.css']
})
export class EntriesComponent {

    @Input()
    public entries : Entry[];
}