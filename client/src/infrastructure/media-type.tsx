import { FunctionComponent } from 'react';
import React from 'react';
import {ChildrenProps} from './redux-types';

interface MediaQueryProps extends ChildrenProps {
  query: string;
}

export const MediaQueryComponent: FunctionComponent<MediaQueryProps> = (props) => {
  const {children, query} = props;
  if (window.matchMedia(query).matches) {
    return children;
  } else {
    return null;
  }
};

export type ScreenSizeEnum = 'phone' | 'tablet' | 'wide' | 'not-phone' | 'mobile';

interface ScreenSizeProps extends ChildrenProps {
  size: ScreenSizeEnum;
}

const mediaQueryStrings = {
  phone: '(max-width: 479px)',
  tablet: '(min-width: 480px) and (max-width: 999px)',
  mobile: '(max-width: 999px)',
  wide: '(min-width: 1000px)',
  'not-phone': '(min-width: 480px)',
};

export const ScreenSizeComponent: FunctionComponent<ScreenSizeProps> = (props) => {
  if (props.children && props.size) {
    return (
      <MediaQueryComponent query={mediaQueryStrings[props.size]}>
        {props.children}
      </MediaQueryComponent>
    );
  }
  return null;
};
