interface DataStore {

    folders : Folder[];
}

interface Folder {

    feeds : Feed[];

    name : string;
}

interface Feed {

    name : string;

    entries : Entry[];

    filterType : FilterType;

    filterValue : string;

    url : string;

    interval : number;

    lastUpdated : Date;
}

interface Entry {

    title : string;

    url : string;

    date : Date;
}

type FilterType = 'none' | 'contains' | 'regexp';