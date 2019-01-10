import React, {SFC} from 'react';
import {FieldProps} from 'formik';
import Select from 'react-select';
import {SelectItem} from './select-item';

interface Props extends FieldProps {
  className: string;
  label: string;
  items: SelectItem[];
  loading: boolean;
}

export const MultiSelectField: SFC<Props> = ({className, label, field, items, form, loading}) => {

  const handleChange = (data: any) => {
    const dataArray = data as SelectItem[];
    form.setFieldValue(field.name, dataArray.map((item) => item.value));
  };

  const selectedValues = items.filter((x) => (field.value as number[]).includes(x.value));

  return (
    <div className={className} style={{margin: '1rem 0'}}>
      <label htmlFor={field.name}>{label}</label>
      <Select
        name={field.name}
        options={items}
        isClearable={true}
        isSearchable={false}
        onChange={handleChange}
        value={selectedValues}
        isLoading={loading}
        isMulti={true}/>
    </div>
  );
};
