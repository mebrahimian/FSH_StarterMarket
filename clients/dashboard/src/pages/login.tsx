import {
    useEffect,
    useRef,
    useState,
    type PointerEvent as ReactPointerEvent,
    type SyntheticEvent,
} from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import {
    AlertCircle,
    ArrowLeft,
    Eye,
    EyeOff,
    Loader2,
    ShieldCheck,
    Sparkles,
    TimerOff,
} from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { consumeSignedOutReason } from "@/auth/inactivity";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { DemoAccountsDialog } from "@/components/auth/demo-accounts-dialog";
import { ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";
import type { DemoAccount } from "@/pages/login.demo-accounts";

type LocationState = { from?: { pathname: string } };

export function LoginPage() {
    const { isAuthenticated, login } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const from =
        (location.state as LocationState | null)?.from?.pathname ?? "/";

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [tenant, setTenant] = useState(env.defaultTenant);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [demoOpen, setDemoOpen] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    const [notice, setNotice] = useState<string | null>(null);
    const [mobileDrag, setMobileDrag] = useState({
        x: 0,
        y: 80,
    });

    const dragStart = useRef<{
        pointerX: number;
        pointerY: number;
        x: number;
        y: number;
    } | null>(null);
    useEffect(() => {
        if (consumeSignedOutReason() === "inactivity") {
            setNotice("به دلیل عدم فعالیت، نشست قبلی شما پایان یافت.");
        }
    }, []);

    if (isAuthenticated) {
        return <Navigate to={from} replace />;
    }

    const performLogin = async (creds: {
        email: string;
        password: string;
        tenant: string;
    }) => {
        setError(null);
        setSubmitting(true);

        try {
            await login(creds);
            navigate(from, { replace: true });
        } catch (err) {
            const message =
                err instanceof ApiRequestError
                    ? err.problem?.detail ??
                    err.problem?.title ??
                    err.message
                    : err instanceof Error
                        ? err.message
                        : "ورود به سامانه ناموفق بود.";

            setError(message);
        } finally {
            setSubmitting(false);
        }
    };

    const onSubmit = async (
        event: SyntheticEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        await performLogin({
            email,
            password,
            tenant,
        });
    };
    const onPickDemo = (account: DemoAccount) => {
        setEmail(account.email);
        setPassword(account.password);
        setTenant(account.tenant);

        void performLogin({
            email: account.email,
            password: account.password,
            tenant: account.tenant,
        });
    };
    const handleDragStart = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        if (window.innerWidth >= 1024) {
            return;
        }

        event.currentTarget.setPointerCapture(event.pointerId);

        dragStart.current = {
            pointerX: event.clientX,
            pointerY: event.clientY,
            x: mobileDrag.x,
            y: mobileDrag.y,
        };
    };

    const handleDragMove = (
        event: ReactPointerEvent<HTMLDivElement>,
    ) => {
        if (!dragStart.current || window.innerWidth >= 1024) {
            return;
        }

        setMobileDrag({
            x:
                dragStart.current.x +
                event.clientX -
                dragStart.current.pointerX,

            y:
                dragStart.current.y +
                event.clientY -
                dragStart.current.pointerY,
        });
    };

    const handleDragEnd = () => {
        dragStart.current = null;
    };
    return (
        <>
            <div className="relative min-h-screen overflow-hidden bg-[#faf7f8]">

                {/* Backgrounds */}
                <div
                    className="pointer-events-none absolute inset-0 z-0 overflow-hidden"
                    aria-hidden="true"
                >
                    {/* Mobile background */}
                    <img
                        src="/branding/sadaf2/sadaf-splash-bgm.png"
                        alt=""
                        className="block h-full w-full object-contain object-top lg:hidden"
                    />

                    {/* Desktop background */}
                    <img
                        src="/branding/sadaf2/sadaf-splash-bg.png"
                        alt=""
                        className="hidden h-full w-full object-cover object-top lg:block"
                    />

                    <div className="absolute inset-0 bg-white/10" />
                </div>

                {/* Real Login */}
                <main
                    dir="rtl"
                    className="relative z-20 flex min-h-screen items-center px-0 py-6 sm:px-8"
                >
                    <section  style={{
                                   "--drag-x": `${mobileDrag.x}px`,
                                   "--drag-y": `${mobileDrag.y}px`,
                                } as React.CSSProperties}
                              className ="relative
                                          [left:var(--drag-x)]
                                          [top:var(--drag-y)]

                                          w-1/2
                                          mx-auto
                                          rounded-[26px]
                                          border
                                          border-white/90
                                          bg-white/[0.985]
                                          p-6
                                          shadow-[0_18px_55px_rgba(30,20,25,0.10)]
                                          backdrop-blur-md

                                          lg:absolute
                                          lg:left-[2.0%]
                                          lg:top-[16%]
                                          lg:w-[25.5%]
                                          lg:max-w-none
                                          lg:mx-0
                                          lg:px-7
                                          lg:py-8"
                    >
                        <div
                            onPointerDown={handleDragStart}
                            onPointerMove={handleDragMove}
                            onPointerUp={handleDragEnd}
                            onPointerCancel={handleDragEnd}
                            className="
    mb-3
    flex
    cursor-grab
    touch-none
    justify-center
    py-1
    active:cursor-grabbing
    lg:hidden
  "
                            title="برای جابه‌جایی بکشید"
                        >
                            <div className="h-1.5 w-12 rounded-full bg-slate-300" />
                        </div>
                        <div className="mb-5">
                            <div // 1) badge «ورود امن»
                                className="mb-3 inline-flex items-center gap-2 rounded-full bg-[var(--color-primary-soft)] px-3 py-1 text-[11px] font-bold text-[var(--color-primary)]">

                                <ShieldCheck className="size-3.5" />
                                ورود امن
                            </div>

                            <h1 className="text-[25px] font-black tracking-tight text-slate-950">
                                ورود به سامانه
                            </h1>

                            <p className="mt-1.5 text-[12px] text-slate-500">
                                تحلیل، کشف، تصمیم بهتر
                            </p>
                        </div>

                        {notice && (
                            <div
                                role="status"
                                className="mb-4 flex items-start gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2.5 text-[11px] leading-5 text-slate-600"
                            >
                                <TimerOff className="mt-0.5 size-4 shrink-0" />
                                <span>{notice}</span>
                            </div>
                        )}

                        <form
                            onSubmit={onSubmit}
                            className="space-y-4"
                            noValidate
                            aria-describedby={error ? "login-error" : undefined}
                        >
                            <div className="space-y-1.5">
                                <Label
                                    htmlFor="email"
                                    className="text-[12px] font-bold text-slate-700"
                                >
                                    ایمیل
                                </Label>

                                <Input
                                    id="email"
                                    type="email"
                                    value={email}
                                    onChange={(event) => setEmail(event.target.value)}
                                    placeholder="name@example.com"
                                    autoComplete="email"
                                    required
                                    aria-invalid={error ? true : undefined}
                                    className="h-11 rounded-xl border-slate-200 bg-white text-left text-[13px] shadow-none focus-visible:ring-rose-400"
                                    dir="ltr"
                                />
                            </div>

                            <div className="space-y-1.5">
                                <div className="flex items-center justify-between">
                                    <Label
                                        htmlFor="password"
                                        className="text-[12px] font-bold text-slate-700"
                                    >
                                        رمز عبور
                                    </Label>

                                    <Link
                                        to="/forgot-password"
                                        className="text-[10.5px] font-semibold text-rose-600 hover:text-rose-700"
                                    >
                                        رمز را فراموش کرده‌اید؟
                                    </Link>
                                </div>

                                <div className="relative">
                                    <Input
                                        id="password"
                                        type={showPassword ? "text" : "password"}
                                        value={password}
                                        onChange={(event) => setPassword(event.target.value)}
                                        placeholder="رمز عبور"
                                        autoComplete="current-password"
                                        required
                                        aria-invalid={error ? true : undefined}
                                        className="h-11 rounded-xl border-slate-200 bg-white pl-11 text-[13px] shadow-none focus-visible:ring-rose-400"
                                    />

                                    <button
                                        type="button"
                                        onClick={() =>
                                            setShowPassword((value) => !value)
                                        }
                                        aria-label={
                                            showPassword
                                                ? "پنهان کردن رمز عبور"
                                                : "نمایش رمز عبور"
                                        }
                                        className="absolute left-3 top-1/2 grid size-8 -translate-y-1/2 place-items-center rounded-lg text-slate-400 transition hover:bg-slate-100 hover:text-slate-700"
                                    >
                                        {showPassword ? (
                                            <EyeOff className="size-4" />
                                        ) : (
                                            <Eye className="size-4" />
                                        )}
                                    </button>
                                </div>
                            </div>

                            <details className="rounded-xl border border-slate-200/80 bg-slate-50/80 px-3.5 py-2">
                                <summary className="cursor-pointer select-none text-[10.5px] font-semibold text-slate-500">
                                    تنظیمات فضای کاری
                                </summary>

                                <div className="mt-3 space-y-1.5">
                                    <Label
                                        htmlFor="tenant"
                                        className="text-[11px] font-semibold text-slate-600"
                                    >
                                        Tenant
                                    </Label>

                                    <Input
                                        id="tenant"
                                        value={tenant}
                                        onChange={(event) =>
                                            setTenant(event.target.value)
                                        }
                                        placeholder="root"
                                        autoComplete="organization"
                                        required
                                        aria-invalid={error ? true : undefined}
                                        className="h-10 rounded-lg bg-white text-[13px]"
                                        dir="ltr"
                                    />
                                </div>
                            </details>

                            {error && (
                                <div
                                    id="login-error"
                                    role="alert"
                                    className="flex items-start gap-2 rounded-xl border border-red-200 bg-red-50 px-3 py-2.5 text-[11px] leading-5 text-red-700"
                                >
                                    <AlertCircle className="mt-0.5 size-4 shrink-0" />
                                    <span>{error}</span>
                                </div>
                            )}

                            <button
                                type="submit"
                                disabled={
                                    submitting ||
                                    !email ||
                                    !password ||
                                    !tenant
                                }
                                className="group flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-gradient-to-l from-[var(--color-primary)] to-[var(--color-primary-hover)] px-4 text-[13px] font-black text-white shadow-[0_12px_28px_oklch(from_var(--color-primary)_l_c_h_/_0.24)] transition hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:translate-y-0"
                            >
                                {submitting ? (
                                    <>
                                        <Loader2 className="size-4 animate-spin" />
                                        <span>در حال ورود…</span>
                                    </>
                                ) : (
                                    <>
                                        <span>ورود به صدف بورس</span>
                                        <ArrowLeft className="size-4 transition-transform group-hover:-translate-x-0.5" />
                                    </>
                                )}
                            </button>
                        </form>

                        {env.demoMode && (
                            <button
                                type="button"
                                onClick={() => setDemoOpen(true)}
                                className="mt-4 flex h-10 w-full items-center justify-center gap-2 rounded-xl border border-dashed border-rose-200 bg-rose-50/60 text-[11px] font-bold text-rose-600 transition hover:bg-rose-50"
                            >
                                <Sparkles className="size-3.5" />
                                ورود با حساب آزمایشی
                            </button>
                        )}
                    </section>
                </main>

                {env.demoMode && (
                    <DemoAccountsDialog
                        open={demoOpen}
                        onOpenChange={setDemoOpen}
                        onPick={onPickDemo}
                    />
                )}
            </div>
        </>
    );
}