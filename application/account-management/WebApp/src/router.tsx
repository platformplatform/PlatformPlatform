import { createBrowserRouter } from "react-router-dom";
import Root from "@/routes/root.tsx";
import ErrorPage from "@/error-page.tsx";
import { CreateTenantForm } from "./ui/tenant/CreateTenantForm";
import { CreatedTenantSuccess } from "./ui/tenant/CreatedTenantSuccess";

export const router = createBrowserRouter([
  {
    path: "/",
    element: <Root />,
    errorElement: <ErrorPage />,
    children: [
      {
        path: "/tenant/:id",
        element: <h1>Tenant</h1>,
      },
      {
        path: "/tenant/create",
        element: <CreateTenantForm />,
      },
      {
        path: "/tenant/create/success",
        element: <CreatedTenantSuccess />,
      },
    ],
  },
]);
