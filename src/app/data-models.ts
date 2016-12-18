export interface DataStore {

    folders : Folder[];
}

export interface Folder {

    feeds : Feed[];

    name : string;
}

export interface Feed {

    name : string;

    entries : Entry[];

    filterType : FilterType;

    filterValue : string;

    url : string;

    interval : number;

    lastUpdated : Date;
}

export interface Entry {

    title : string;

    url : string;

    date : Date;
}

export type FilterType = 'none' | 'contains' | 'regexp';