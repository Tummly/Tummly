export const isValidEmail = (email: string): boolean => {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
};

export const isRequired = (value: unknown): boolean => {
  return Boolean(value && value.toString().trim() !== "");
};

export const isValidPassword = (password: string): boolean => {
  return password.length >= 8;
};

export const isValidMobile = (mobile: string): boolean => {
  return /^[0-9+\-\s()]+$/.test(mobile);
};
