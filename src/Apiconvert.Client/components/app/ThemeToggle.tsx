"use client";

import { useTheme } from "next-themes";
import {
  DropdownMenuItem,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  return (
    <>
      <DropdownMenuSeparator />
      <DropdownMenuItem onClick={() => setTheme("system")}>
        Theme: System {theme === "system" ? "✓" : ""}
      </DropdownMenuItem>
      <DropdownMenuItem onClick={() => setTheme("light")}>
        Theme: Light {theme === "light" ? "✓" : ""}
      </DropdownMenuItem>
      <DropdownMenuItem onClick={() => setTheme("dark")}>
        Theme: Dark {theme === "dark" ? "✓" : ""}
      </DropdownMenuItem>
    </>
  );
}
