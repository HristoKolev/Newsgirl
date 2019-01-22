import React, { SFC } from 'react';
import { Card, CardBody } from 'mdbreact';
import { ErrorMessagesContainer } from '../infrastructure/errors/components/ErrorComponents';
import { Form, Formik, Field } from 'formik';
import BackButton from '../infrastructure/components/BackButton';
import { fields } from '../infrastructure/fields/fields';
import { FeedDto } from './feeds.dto';

interface Props {
  model: FeedDto;
  errorMessages: string[];
  onSubmit: (data: FeedDto | undefined) => void;
}

export const PostFormComponent: SFC<Props> = ({errorMessages, model,   onSubmit}) => (
  <Formik initialValues={model} onSubmit={onSubmit}>
    <Form>
      <button type="submit" className="btn btn-md btn-default Ripple-parent">Save</button>

      <BackButton render={(props) =>
        <a {...props} className="btn btn-md btn-warning Ripple-parent">Back</a>
      }/>

      <hr/>

      <ErrorMessagesContainer errorMessages={errorMessages}/>

      {errorMessages && !!errorMessages.length && <hr/>}

      <hr/>

      <Card border="danger" className="mb-3">
        <CardBody className="text-default">

          <Field
            component={fields.TextField}
            label="Name"
            name="feedName"
          />

          <Field
            component={fields.TextField}
            label="Url"
            name="feedUrl"
          />

        </CardBody>
      </Card>
    </Form>
  </Formik>
);
