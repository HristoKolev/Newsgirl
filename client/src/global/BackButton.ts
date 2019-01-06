import { SFC } from 'react';
import { withRouter } from 'react-router-dom';
import { RouteComponentProps } from 'react-router';
import { RenderProps } from '../infrastructure/redux-types';

interface RenderComponentProps {
  onClick: (e: any) => void;
}

interface Props extends RouteComponentProps<any>, RenderProps<RenderComponentProps> {
}

const BackButton: SFC<Props> = ({render, history}) => render({
  onClick: (e) => {
    e.preventDefault();
    history.goBack();
  },
});

export default withRouter(BackButton);
