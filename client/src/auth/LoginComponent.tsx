import React from 'react';
import LoadingSpinner from '../infrastructure/components/LoadingSpinner';

import './LoginComponent.scss';
import {ErrorMessagesContainer} from '../infrastructure/errors/components/ErrorComponents';
import {fields} from '../infrastructure/fields/fields';
import {Button, Card, CardBody} from 'mdbreact';
import {LoginRequest, LoginResponse} from '../infrastructure/auth-dto';
import {AppContext} from '../infrastructure/context';
import {Field, Form, Formik} from 'formik';
import {RouteComponentProps} from 'react-router';
import {BaseComponent} from '../infrastructure/components/BaseComponent';

interface Props extends RouteComponentProps<{}> {
  context: AppContext;
}

interface State {
  errorMessages: string[];
  loading: boolean;
  model: LoginRequest | undefined;
}

export class LoginComponent extends BaseComponent<Props, State> {
  state: State = {
    errorMessages: [],
    loading: false,
    model: undefined,
  };

  login = async (values: {username: string; password: string}) => {
      const {api, allActions } = this.props.context;
      const response = await api.send<LoginRequest, LoginResponse>(
        'LoginRequest', { username: values.username, password: values.password });
      if (response.success) {
        allActions.session.login(response.payload);
        this.props.history.push('/');
      } else {
        await this.setStateAsync({errorMessages: response.errorMessages});
      }
  };

  render() {
    return (
      <Formik initialValues={this.state.model} onSubmit={this.unwrapPromise(this.login)}>
        <Form>
          <Card className="login-form">
            <CardBody>
              <p className="h4 text-center py-4">Login</p>

              <div className="grey-text">

                <Field
                  component={fields.TextField}
                  name="username"
                  icon="user"
                  label="Username"
                />

                <Field
                  component={fields.PasswordField}
                  name="password"
                  icon="lock"
                  label="Password"
                />

              </div>

              <div className="text-center py-3 mt-3">
                <Button color="warning" block disabled={this.state.loading} type="submit">Login</Button>
              </div>

              <ErrorMessagesContainer errorMessages={this.state.errorMessages}/>

              {this.state.loading && <LoadingSpinner> Please, wait...</LoadingSpinner>}
            </CardBody>
          </Card>
        </Form>
      </Formik>
    );
  }
}
