import React, { Component } from 'react';

import { Collapse, Navbar, NavbarBrand, NavbarNav, NavbarToggler, NavItem, NavLink } from 'mdbreact';

import SessionComponent from '../components/SessionComponent';
import LogOutButton from '../components/LogOutButton';

interface State {
}

export class Header extends Component<any, State> {

  state = {
    collapse: false,
    isWideEnough: false,
  };

  onClick = () => {
    this.setState({
      collapse: !this.state.collapse,
    });
  };

  render() {
    return (
      <Navbar color="red" dark expand="md" fixed="top">
        <NavbarBrand href="#">
          <strong>Newsgirl</strong>
        </NavbarBrand>
        {!this.state.isWideEnough && <NavbarToggler onClick={this.onClick}/>}
        <Collapse isOpen={this.state.collapse} navbar>
          <NavbarNav left>
            <NavItem>
              <SessionComponent>
                <NavLink to={'/feeds'}>Feeds</NavLink>
              </SessionComponent>
            </NavItem>
          </NavbarNav>
          <NavbarNav right>
            <NavItem>
              <SessionComponent>
                <LogOutButton render={(props) =>
                  <a className="nav-link Ripple-parent" {...props}>Logout</a>
                }
                />
              </SessionComponent>
            </NavItem>
            <NavItem>
              <SessionComponent isLoggedIn={false}>
                <NavLink to={'/login'}>Login</NavLink>
              </SessionComponent>
            </NavItem>
          </NavbarNav>
        </Collapse>
      </Navbar>
    );
  }
}
