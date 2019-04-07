import { FunctionComponent } from 'react';
import { withRouter } from 'react-router-dom';
import { RouteComponentProps } from 'react-router';
import { RenderProps } from '../redux-types';

interface RenderComponentProps {
  onClick: (e: any) => void;
}

interface Props extends RouteComponentProps<any>, RenderProps<RenderComponentProps> {
}

const BackButton: FunctionComponent<Props> = ({render, history}) => render({
  onClick: (e) => {
    e.preventDefault();
    history.goBack();
  },
});

export default withRouter(BackButton);
