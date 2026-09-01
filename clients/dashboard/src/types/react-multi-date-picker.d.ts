import "react-multi-date-picker";
import type { Calendar } from "react-date-object";

declare module "react-multi-date-picker" {
    interface CalendarProps<
        Multiple extends boolean = false,
        Range extends boolean = false,
    > {
        calendar?:
        | Calendar
        | Omit<Calendar, "leapsLength">;
    }
}