
export class ComponentUnmountedError extends Error {
  constructor(message: string) {
    super(message);
    Object.assign(this, {
      isComponentUnmountedError: true,
    });
  }
}

export const isComponentUnmountedError = (error: Error): error is ComponentUnmountedError => {
  return !!(error as any).isComponentUnmountedError;
};
