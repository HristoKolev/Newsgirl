import React, { SFC } from 'react';
import { FieldProps } from 'formik';

import './CheckboxField.scss';

interface Props extends FieldProps {
  className: string;
  label: string;
}

export const CheckboxField: SFC<Props> = ({className, label, field, form}) => {

  const labelClick = () => {
    form.setFieldValue(field.name, !field.value);
  };

  return (
    <div className={className + ' checkbox-field'} onClick={labelClick}>
      <input type="checkbox"
             className="form-control form-check-input" value=""
             name={field.name}
             checked={field.value}
             onChange={field.onChange}
      />
      <label className="form-check-label mr-5">{label}</label>
    </div>
  );
};
