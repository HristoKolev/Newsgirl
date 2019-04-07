import React, { FunctionComponent } from 'react';
import { FieldProps } from 'formik';

import './TextArea.scss';

interface Props extends FieldProps {
  className: string;
  label: string;
}

export const TextAreaField: FunctionComponent<Props> = ({className, label, field}) => (
  <div className={'textarea-field ' + className}>
    <label>
      <div>{label}</div>
      <textarea value={field.value || ''}
                onChange={field.onChange}
                name={field.name}
                placeholder={label}/>
    </label>
  </div>
);
