import { Action, AnyAction, ReducersMapObject, Store } from 'redux';
import { AppContext, ComputedAppContext } from './context';

export interface ReduxState {
}

export interface AllActionCreators {
}

export type ReduxStore = Store<ReduxState> & {
  initContext: (state: ReduxState) => ComputedAppContext,
};

export interface SagaObject {
  actionType: string;
  saga: (action: AnyAction, context: AppContext) => IterableIterator<any>;
  failedActionCreator?: (error: any) => Action;
}

export interface CancellableSagaObject extends SagaObject {
  cancelActionType: string;
  canceledActionCreator: () => Action;
}

export interface SagaMap {
  [key: string]: SagaObject | CancellableSagaObject;
}

export interface StoreOptions {
  reducers: ReducersMapObject;
  sagas: SagaMap;
  persistentFields: string[];
  initContext: (state: ReduxState) => ComputedAppContext;
  middleware: any[];

}

export interface RenderProps<TProps> {
  render: (props: TProps) => any;
}

export type ChildrenType = any;

export type DefaultProps<Props> = Props | any;

export interface ChildrenProps {
  children: ChildrenType;
}
