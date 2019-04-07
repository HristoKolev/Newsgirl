import React from 'react';
import { ChildrenProps } from '../redux-types';

import './StandardFrame.scss';

interface Props extends ChildrenProps {
}

export const StandardFrame = ({children}: Props) => (
  <section className="standard-frame">
    {children}
  </section>
);
