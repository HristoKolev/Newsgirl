import { FunctionComponent } from 'react';
import { connect } from 'react-redux';
import { ReducerErrorOptions, SagaError } from '../errors';
import { RenderProps } from '../../redux-types';
import { errorsActionCreators, ErrorsActionCreators, ErrorsState } from '../errors.state';
import { routerActionCreators, RouterActionCreators } from '../../global.state';
import { select } from '../../store-helpers';
import { wrapActions } from '../../store';

interface RenderComponentProps {
  onClose: () => void;
  error: string[] | null;
  sagaError: SagaError | null;
  reducerError: ReducerErrorOptions | null;
}

interface Props extends RenderProps<RenderComponentProps> {
  actions: ErrorsActionCreators & RouterActionCreators;
  state: ErrorsState;
}

const GlobalErrorComponent: FunctionComponent<Props> = ({state: {error, sagaError, reducerError}, actions, render}) => {

  if (error === null && sagaError === null && reducerError === null) {
    return null;
  }

  return render({
    onClose: () => {
      actions.clearErrors();
      actions.routerPush('/');
    },
    error,
    sagaError,
    reducerError,
  });
};

export default connect(
  select((state) => state.errors),
  wrapActions(errorsActionCreators, routerActionCreators),
)(GlobalErrorComponent);
