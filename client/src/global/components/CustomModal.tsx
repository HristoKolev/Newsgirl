import React, { SFC } from 'react';
import { ChildrenProps } from '../../infrastructure/redux-types';

import { Modal, ModalBody } from 'mdbreact';

interface Props extends ChildrenProps {
  title: string;
  onClose: () => void;
  className?: string;
}

export const CustomModal: SFC<Props> = ({title, children, onClose}) => (
  <div className="modal">
    <Modal isOpen={true} size="lg">
      <div className="modal-header danger-color white-text">
        <h4 className="title">{title}</h4>
        <button type="button" className="close" onClick={onClose}>
          <span aria-hidden="true">×</span>
        </button>
      </div>
      <ModalBody className="grey-text">
        {children}
      </ModalBody>
    </Modal>
  </div>
);

CustomModal.defaultProps = {
  onClose: () => {
  },
};
