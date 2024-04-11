import { NextRequest, NextResponse } from "next/server";
import {
  getLocaleOrDefault,
  getNegotiatedLocale,
  getUserLocale,
} from "./translations/resolveLocale";
import { LANGUAGE_COOKIE } from "./translations/i18n";

function languageMiddleware(request: NextRequest) {
  const pathname = request.nextUrl.pathname;

  if (
    /^\/static/.test(pathname) ||
    /^\/images/.test(pathname) ||
    [
      "/manifest.json",
      "/favicon.ico",
      // Your other files in `public`
    ].includes(pathname)
  ) {
    return;
  }
  // Check if there is any supported locale in the cookie
  const userLocale = getUserLocale(request.cookies);

  if (userLocale == null) {
    const locale = getLocaleOrDefault(getNegotiatedLocale(request.headers));

    const response = NextResponse.next();
    response.cookies.set(LANGUAGE_COOKIE, locale, {
      path: "/",
      httpOnly: true,
      sameSite: "strict",
      maxAge: 31536000,
    });

    return response;
  }
}

export function middleware(request: NextRequest) {
  return languageMiddleware(request);
}

export const config = {
  // Matcher ignoring `/_next/` and `/api/`
  matcher: ["/((?!api|_next/static|_next/image|policy).*)"],
};
