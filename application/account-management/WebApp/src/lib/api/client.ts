import { makeApi, Zodios, type ZodiosOptions } from "@zodios/core";
import { z } from "zod";

const UpdateTenantCommand = z.object({ name: z.string().nullable(), phone: z.string().nullable() }).partial();
const CreateTenantCommand = z
  .object({
    subdomain: z.string().nullable(),
    name: z.string().nullable(),
    phone: z.string().nullable(),
    email: z.string().nullable(),
  })
  .partial();
const UserRole = z.enum(["TenantUser", "TenantAdmin", "TenantOwner"]);
const UpdateUserCommand = z.object({ userRole: UserRole, email: z.string().nullable() }).partial();
const TenantId = z.object({ value: z.string().nullable() }).partial();
const CreateUserCommand = z.object({ tenantId: TenantId, email: z.string().nullable(), userRole: UserRole }).partial();

export const schemas = {
  UpdateTenantCommand,
  CreateTenantCommand,
  UserRole,
  UpdateUserCommand,
  TenantId,
  CreateUserCommand,
};

const endpoints = makeApi([
  {
    method: "post",
    path: "/api/tenants",
    alias: "postApitenants",
    requestFormat: "json",
    parameters: [
      {
        name: "body",
        type: "Body",
        schema: CreateTenantCommand,
      },
    ],
    response: z.void(),
  },
  {
    method: "get",
    path: "/api/tenants/:id",
    alias: "getApitenantsId",
    requestFormat: "json",
    parameters: [
      {
        name: "id",
        type: "Path",
        schema: z.string(),
      },
    ],
    response: z.void(),
  },
  {
    method: "put",
    path: "/api/tenants/:id",
    alias: "putApitenantsId",
    requestFormat: "json",
    parameters: [
      {
        name: "body",
        type: "Body",
        schema: UpdateTenantCommand,
      },
      {
        name: "id",
        type: "Path",
        schema: z.string(),
      },
    ],
    response: z.void(),
  },
  {
    method: "delete",
    path: "/api/tenants/:id",
    alias: "deleteApitenantsId",
    requestFormat: "json",
    parameters: [
      {
        name: "id",
        type: "Path",
        schema: z.string(),
      },
    ],
    response: z.void(),
  },
  {
    method: "post",
    path: "/api/users",
    alias: "postApiusers",
    requestFormat: "json",
    parameters: [
      {
        name: "body",
        type: "Body",
        schema: CreateUserCommand,
      },
    ],
    response: z.void(),
  },
  {
    method: "get",
    path: "/api/users/:id",
    alias: "getApiusersId",
    requestFormat: "json",
    parameters: [
      {
        name: "id",
        type: "Path",
        schema: z.string(),
      },
    ],
    response: z.void(),
  },
  {
    method: "put",
    path: "/api/users/:id",
    alias: "putApiusersId",
    requestFormat: "json",
    parameters: [
      {
        name: "body",
        type: "Body",
        schema: UpdateUserCommand,
      },
      {
        name: "id",
        type: "Path",
        schema: z.string(),
      },
    ],
    response: z.void(),
  },
  {
    method: "delete",
    path: "/api/users/:id",
    alias: "deleteApiusersId",
    requestFormat: "json",
    parameters: [
      {
        name: "id",
        type: "Path",
        schema: z.string(),
      },
    ],
    response: z.void(),
  },
]);

export const api = new Zodios(endpoints);

export function createApiClient(baseUrl: string, options?: ZodiosOptions) {
  return new Zodios(baseUrl, endpoints, options);
}
