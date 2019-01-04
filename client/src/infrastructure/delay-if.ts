import {Observable, MonoTypeOperatorFunction, Subject, Subscription} from 'rxjs';

export type delayCondition<T> = (value: T) => boolean;

export function delayWhen<T>(
  predicate: delayCondition<T>,
  delayInterval: number): MonoTypeOperatorFunction<T> {
  return (source: Observable<T>) => {
    const subject = new Subject<T>();

    let subscription: Subscription;

    const doWork = () => {
      if (subject.closed) {
        return;
      }
      subscription = source.subscribe((x) => {
        if (predicate(x)) {
          subscription.unsubscribe();
          setTimeout(() => {
            subject.next(x);
            doWork();
          }, delayInterval);
        } else {
          subject.next(x);
        }
      });
    };

    doWork();

    return subject;
  };
}

export function fromPromiseCustom<T, P>(promiseFactory: (x: T) => Promise<P>) {
  return (source: Observable<T>): Observable<P> => {

    const subject = new Subject<P>();

    let subscription: Subscription;

    const doWork = () => {
      if (subject.closed) {
        return;
      }
      subscription = source.subscribe((x) => {
        subscription.unsubscribe();
        const promise = promiseFactory(x);
        promise.then((value) => {
          if (!subject.closed) {
            subject.next(value);
            doWork();
          }
        }, (error) => {
          subject.error(error);
        });
      });
    };

    doWork();

    return subject;
  };
}
