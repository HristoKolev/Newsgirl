import React, { FunctionComponent } from 'react';
import {FieldProps} from 'formik';
import Select from 'react-select';
import {SelectItem} from './select-item';

interface Props extends FieldProps {
  className: string;
  label: string;
  items: SelectItem[];
}

export const SelectField: FunctionComponent<Props> = ({className, label, field, items, form}) => {

  let handleChange = (data: any) => {
    const newValue = data as SelectItem;
    const value = newValue ? newValue.value : undefined;
    form.setFieldValue(field.name, value);
  };

  let selectedValue: any[] = [];

  if (items) {
    const value = field.value;
    if (value || value === 0) {
      selectedValue = items.filter((x) => x.value === value);
      if (!selectedValue.length) {
        throw new Error(`Value '${value}' does not exist in the items array. `);
      }
    }
  } else {
    selectedValue = [undefined];
    handleChange = () => {
    };
  }

  return (
    <div className={className} style={{margin: '1rem 0'}}>
      <label htmlFor={field.name}>{label}</label>
      <Select
        name={field.name}
        options={items}
        isClearable={true}
        isSearchable={false}
        onChange={handleChange}
        value={selectedValue[0]}
        isLoading={!items}
      />
    </div>
  );
};
