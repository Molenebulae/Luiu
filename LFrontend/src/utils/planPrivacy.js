export const PLAN_PRIVACY = {
  PRIVATE: 0,
  PUBLIC: 1,
  SUGGEST: 2,
}

export const PLAN_OFFICE_OPER = {
  DEFAULT: 0,
  RECOMMEND: 1,
  BLOCKED: 2,
}

export const normalizePrivacyStatus = (value) => {
  const status = Number(value)

  return Object.values(PLAN_PRIVACY).includes(status) ? status : PLAN_PRIVACY.PRIVATE
}

export const normalizeOfficeOperStatus = (value) => {
  const status = Number(value)

  return Object.values(PLAN_OFFICE_OPER).includes(status) ? status : PLAN_OFFICE_OPER.DEFAULT
}

export const isPublicPrivacyStatus = (value) => {
  const status = normalizePrivacyStatus(value)

  return status === PLAN_PRIVACY.PUBLIC || status === PLAN_PRIVACY.SUGGEST
}

export const isSuggestPrivacyStatus = (value) => normalizePrivacyStatus(value) === PLAN_PRIVACY.SUGGEST

export const resolvePrivacySuggest = (value) => isSuggestPrivacyStatus(value)

export const isOfficeRecommendedStatus = (value) =>
  normalizeOfficeOperStatus(value) === PLAN_OFFICE_OPER.RECOMMEND
