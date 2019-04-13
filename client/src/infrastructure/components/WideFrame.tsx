import React from 'react';
import { ChildrenProps } from '../redux-types';

import './WideFrame.scss';

interface Props extends ChildrenProps {
}

export const WideFrame = ({children}: Props) => (
  <div className="wide-frame">
    {children}
  </div>
);
