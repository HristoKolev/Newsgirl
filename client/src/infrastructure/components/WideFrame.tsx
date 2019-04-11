import React from 'react';
import { ChildrenProps } from '../redux-types';

import './WideFrame.scss';

interface Props extends ChildrenProps {
}

export const WideFrame = ({children}: Props) => (
  <section className="wide-frame">
    {children}
  </section>
);
