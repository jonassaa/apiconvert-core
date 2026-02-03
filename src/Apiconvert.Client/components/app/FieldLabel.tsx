import { Label } from "@/components/ui/label";
import { TooltipIcon } from "@/components/app/TooltipIcon";

export function FieldLabel({
  htmlFor,
  label,
  tooltip,
}: {
  htmlFor?: string;
  label: string;
  tooltip: string;
}) {
  return (
    <div className="flex items-center gap-2">
      <Label htmlFor={htmlFor}>{label}</Label>
      <TooltipIcon text={tooltip} />
    </div>
  );
}
