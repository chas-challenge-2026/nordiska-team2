import { Outlet } from "react-router-dom";
import Navbar from "./navigation/Navbar";
import MobileNavbar from "./navigation/MobileNavbar"
import BlobBackground from "../../components/ui/BlobBackground";

export default function AuthenticatedLayout() {
  return (
    <div className="grid min-h-screen grid-cols-1 
                    lg:grid-cols-12">
      <Navbar />
      <MobileNavbar />

      <main className="relative isolate flex min-w-0 flex-col bg-background p-4 
                      sm:px-6 sm:pt-6 pb-24
                      lg:col-span-10 lg:pb-6
                      ">
        <BlobBackground />
        <Outlet />
      </main>
    </div>

    
  )
}