export type Result<T = undefined> = ({ payload: T; success: true; } | { success: false; errorMessages: string[]; });
