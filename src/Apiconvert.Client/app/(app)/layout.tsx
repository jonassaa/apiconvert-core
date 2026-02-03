"use client";

import { useEffect, useState, type ReactNode } from "react";
import { useRouter } from "next/navigation";
import { createClient } from "@/lib/supabase/client";

export default function AppLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const supabase = createClient();
    let isActive = true;

    supabase.auth.getSession().then(({ data }) => {
      if (!isActive) return;
      if (!data.session) {
        router.replace("/login");
        return;
      }
      setReady(true);
    });

    return () => {
      isActive = false;
    };
  }, [router]);

  if (!ready) {
    return <div className="page-shell" />;
  }

  return <div className="page-shell">{children}</div>;
}
