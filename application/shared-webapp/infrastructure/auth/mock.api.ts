import type { UserInfo } from "./actions";

const mockAuthUserInfo: UserInfo = {
  isAuthenticated: true,
  locale: "en-US",
  email: "foo@bar.com",
  tenantId: "acme",
  userRole: "TenantUser",
  userName: "Foo"
};

const mockAnonymousUserInfo: UserInfo = {
  isAuthenticated: false,
  locale: "en-US"
};

export const accountManagementApi = {
  async GET(_route: "/api/auth/user-info") {
    return {
      data: {
        ...mockAuthUserInfo
      },
      response: {
        ok: true
      }
    };
  },
  async POST(route: "/api/auth/login" | "/api/auth/logout", _?: unknown) {
    switch (route) {
      case "/api/auth/login":
        return {
          data: {
            ...mockAuthUserInfo
          },
          response: {
            ok: true
          }
          // biome-ignore lint/suspicious/noExplicitAny: This is a mock API
        } as any;
      case "/api/auth/logout":
        return {
          data: {
            ...mockAnonymousUserInfo
          },
          response: {
            ok: true
          }
          // biome-ignore lint/suspicious/noExplicitAny: This is a mock API
        } as any;
    }
  }
};
