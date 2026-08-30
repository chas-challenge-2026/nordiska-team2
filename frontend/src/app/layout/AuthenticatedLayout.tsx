import { Outlet } from "react-router-dom";
import Navbar from "./Navbar";

export default function AuthenticatedLayout() {
  return (
    <div className="grid min-h-screen grid-cols-1 lg:grid-cols-12">
      <Navbar />

      <main className="bg-background p-6 lg:col-span-7">
        <Outlet />
      </main>

      <aside className="border-border lg:col-span-3 lg:border-l">
        ...
      </aside>
    </div>

    
  )
}