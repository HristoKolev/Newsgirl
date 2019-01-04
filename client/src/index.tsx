import React from 'react';
import ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import { Route } from 'react-router-dom';
import { ConnectedRouter, routerMiddleware, routerReducer } from 'react-router-redux';
import { registerObserver } from 'react-perf-devtool';

import 'normalize.css';

import 'font-awesome/css/font-awesome.min.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import 'mdbreact/dist/css/mdb.css';
import 'react-toastify/dist/ReactToastify.css';

import './global/styles.scss';

import { apiClient } from './infrastructure/api-client';
import { createContextObjectFactory } from './infrastructure/context';
import { configureStore } from './infrastructure/store';
import ErrorGuardComponent from './infrastructure/errors/components/ErrorGuardComponent';
import { routerActionCreators, toastActionCreators, toastSagas } from './global/global.state';
import { errorsActionCreators, errorsReducer, errorsSagas } from './infrastructure/errors/errors.state';
import { sessionActionCreators, sessionReducer } from './global/session.state';
import { settingsReducer } from './global/settings.state';

import { App } from './global/layout/App';

import { AuthRoutes } from './auth/auth.module';
import { freezeMiddleware } from './infrastructure/freeze-middleware';
import createHistory from 'history/createBrowserHistory';
import { distributeMiddleware, distributeDispatch } from './infrastructure/distribute-dispatch';

if (process.env.NODE_ENV === 'development') {
  registerObserver();
}

const history = createHistory();

// Configure the store
const store = configureStore({
  reducers: {
    session: sessionReducer,
    settings: settingsReducer,
    errors: errorsReducer,
    router: routerReducer,
  },
  sagas: {
    ...errorsSagas,
    ...toastSagas,
  },
  persistentFields: [
    'session',
  ],
  initContext: ({session}) => ({
    api: apiClient(session.token),
    allActions: {
      session: sessionActionCreators,
      router: routerActionCreators,
      errors: errorsActionCreators,
      toast: toastActionCreators,
    },
  }),
  middleware: [
    freezeMiddleware,
    routerMiddleware(history),
    distributeMiddleware,
  ],
});

distributeDispatch.subscribe((msg) => {
  store.dispatch(msg.payload);
});

const ctof = createContextObjectFactory(store);

const Home = () => <div/>;

const Root = () => (
  <Provider store={store}>
    <ErrorGuardComponent>
      <ConnectedRouter history={history}>
        <App>
          <ErrorGuardComponent>
            <Route exact path="/" component={Home}/>
            <AuthRoutes ctof={ctof}/>
          </ErrorGuardComponent>
        </App>
      </ConnectedRouter>
    </ErrorGuardComponent>
  </Provider>
);

ReactDOM.render(
  <Root/>,
  document.getElementById('root'),
);
