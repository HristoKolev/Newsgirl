import React, { FunctionComponent } from 'react';
import AceEditor from 'react-ace';

import 'brace/mode/yaml';
import 'brace/mode/json';
import 'brace/theme/xcode';

import './EditorField.scss';

import {FieldProps} from 'formik';

interface Props extends FieldProps {
  label: string;
  mode: string;
  theme: string;
}

export const EditorField: FunctionComponent<Props> = ({label, field, form, mode, theme}) => {

  const handleChange = (value: any) => {
    form.setFieldValue(field.name, value);
  };

  return (
    <div className="editor-field">
      <div className="editor-field__label">{label}</div>
      <AceEditor
        mode={mode}
        theme={theme}
        onChange={handleChange}
        name={field.name}
        value={field.value}
        editorProps={{$blockScrolling: true}}
        width="93em"
      /></div>
  );
};
