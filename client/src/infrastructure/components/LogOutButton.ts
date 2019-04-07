import { connect } from 'react-redux';
import { wrapActions } from '../store';
import { SessionActionCreators, sessionActionCreators } from '../session.state';
import { FunctionComponent } from 'react';
import { RenderProps } from '../redux-types';

interface RenderComponentProps {
  onClick: (e: any) => void;
}

interface Props extends RenderProps<RenderComponentProps> {
  actions: SessionActionCreators;
}

const LogOutButton: FunctionComponent<Props> = ({actions, render}) => {

  return render({
    onClick: (e) => {
      e.preventDefault();
      actions.logout();
    },
  });
};

export default connect(null, wrapActions(sessionActionCreators))(LogOutButton);
