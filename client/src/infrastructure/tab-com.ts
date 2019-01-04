import { uuid } from './uuid';

export interface TabComMessage {
  type: string;
  payload: any;
}

export interface TabComSubscriber {
  func: (message: TabComMessage) => void;
  type?: string;
}

export class TabCom {

  private readonly storeKey: string;

  private readonly subscribers: TabComSubscriber[];

  constructor(storeKey: string = 'tab-com-message') {
    this.storeKey = storeKey;
    this.subscribers = [];

    window.onstorage = (e: StorageEvent) => {

      if (e.storageArea !== localStorage || e.key !== this.storeKey) {
        return;
      }

      const message = JSON.parse(e.newValue || '{}') as TabComMessage;

      for (const subscriber of this.subscribers) {

        if (subscriber.type) {
          if (subscriber.type === message.type) {
            subscriber.func(message);
          }
        } else {
          subscriber.func(message);
        }
      }
    };
  }

  subscribe(func: (message: TabComMessage) => void, type?: string): void {
    this.subscribers.push({type, func});
  }

  sendMessage(message: TabComMessage) {
    localStorage.setItem(this.storeKey, JSON.stringify({
      ...message,
      id: uuid(),
    }));
  }
}
