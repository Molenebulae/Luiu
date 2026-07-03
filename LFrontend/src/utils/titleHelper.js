
export const modeTitles = {
  login: '登入',
  register: '註冊',
  verify: '註冊驗證',
  forgot: '忘記密碼',
  reset: '密碼重設',
}

export const updateDocumentTitle = (to) => {
  if (to.name === 'MemberAuth') {
    const modeName = modeTitles[to.params.mode] || '認證';
    document.title = `${modeName} | Luiu`;
  } else {
    document.title = to.meta.title || 'Luiu';
  }
}
