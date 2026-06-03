/*
 =========================================
 EMAIL VALIDATION
 =========================================
*/

export const isValidEmail = (
  email
) => {

  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    .test(email);
};

/*
 =========================================
 REQUIRED FIELD CHECK
 =========================================
*/

export const isRequired = (
  value
) => {

  return (
    value &&
    value.toString().trim() !== ""
  );
};

/*
 =========================================
 PASSWORD VALIDATION
 =========================================
*/

export const isValidPassword = (
  password
) => {

  return password.length >= 8;
};

/*
 =========================================
 MOBILE VALIDATION
 =========================================
*/

export const isValidMobile = (
  mobile
) => {

  return /^[0-9+\-\s()]+$/
    .test(mobile);
};