const compose = (...fns: any[]) => fns.reduce((f, g) => (...args: any[]) => f(g(...args)));

export const decorate = (comp: any) => {

  const decorators: any[] = [];

  const ctx = {
    apply(decorator: any) {
      decorators.push(decorator);
      return ctx;
    },
    fold() {
      return compose.apply(null, decorators)(comp);
    },
  };
  return ctx;
};
