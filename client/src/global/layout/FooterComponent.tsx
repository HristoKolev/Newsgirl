import React, { SFC } from 'react';
import { Footer } from 'mdbreact';

export const FooterComponent: SFC = () => (
  <Footer color="red" className="font-small pt-4 mt-4 footer">
    <div className="footer-copyright text-center py-3">
      &copy; {(new Date().getFullYear())} Copyright: <a href="#"> Newsgirl </a>
    </div>
  </Footer>
);
