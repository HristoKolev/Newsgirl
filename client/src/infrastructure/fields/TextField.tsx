import React, { FunctionComponent } from 'react';
import { Input } from 'mdbreact';
import { FieldProps } from 'formik';

interface Props {
  className: string;
  label: string;
  icon: string;
}

export const TextField: FunctionComponent<FieldProps & Props> = ({className, label, icon, field}) => (
  <div className={className}>
    <Input
      label={label}
      value={field.value || ''}
      onChange={field.onChange}
      name={field.name}
      icon={icon}
      group
      type="text"
    />
  </div>
);
