import { connect } from 'react-redux';
import { SFC } from 'react';
import { select } from '../store-helpers';
import { SessionState } from '../session.state';
import { ChildrenProps } from '../redux-types';

interface Props extends ChildrenProps {
  isLoggedIn?: boolean;
}

interface InjectedProps extends Props {
  state: SessionState;
}

const SessionComponent: SFC<Props> = (props) => {

  const {children, isLoggedIn} = props;
  const {state} = props as InjectedProps;

  if (state.isLoggedIn !== isLoggedIn) {
    return null;
  } else {
    return children || null;
  }
};

SessionComponent.defaultProps = {
  isLoggedIn: true,
};

export default connect(select((state) => state.session))(SessionComponent);
