
import { useState, type SyntheticEvent } from "react";
import { Link, Navigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { AlertCircle, CheckCircle2, Loader2 } from "lucide-react";

import { selfRegister } from "@/auth/api";
import { useAuth } from "@/auth/use-auth";
import { AuthLayout } from "@/components/auth/auth-layout";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ApiRequestError } from "@/lib/api-client";

export function RegisterPage() {
    const { t } = useTranslation("common");
    const { isAuthenticated } = useAuth();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const [submitting, setSubmitting] = useState(false);
    const [registered, setRegistered] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function onSubmit(
        event: SyntheticEvent<HTMLFormElement>,
    ) {
        event.preventDefault();
        setError(null);
        setSubmitting(true);

        try {
            await selfRegister({
                email: email.trim(),
                password,
            });

            setRegistered(true);
        } catch (err) {
            const message =
                err instanceof ApiRequestError
                    ? err.problem?.detail ??
                      err.problem?.title ??
                      err.message
                    : err instanceof Error
                      ? err.message
                      : t("auth.register.registrationFailed");

            setError(message);
        } finally {
            setSubmitting(false);
        }
    }

    if (isAuthenticated) {
        return <Navigate to="/" replace />;
    }

    return (
        <AuthLayout>
            <div className="-mt-10 w-full rounded-[26px] border border-white/90 bg-white/[0.985] p-6 shadow-[0_18px_55px_rgba(30,20,25,0.10)] backdrop-blur-md sm:p-8 min-h-[445px]">
                <div className="mb-6 text-center">
                    <h1 className="text-2xl font-bold">
                        {t("auth.register.title")}
                    </h1>

                    <p className="mt-2 text-sm text-muted-foreground">
                        {t("auth.register.subtitle")}
                    </p>
                </div>

                {registered ? (
                    <div className="space-y-5 text-center">
                        <CheckCircle2
                            className="mx-auto text-emerald-600"
                            size={42}
                        />

                        <h2 className="text-lg font-semibold">
                            {t("auth.register.success")}
                        </h2>

                        <p className="text-sm leading-7 text-muted-foreground">
                            {t("auth.register.successDescription")}
                        </p>

                        <Link
                            to="/login"
                            className="inline-flex rounded-lg bg-primary px-6 py-3 text-sm font-medium text-primary-foreground"
                        >
                            {t("auth.register.backToLogin")}
                        </Link>
                    </div>
                ) : (
                    <form
                        onSubmit={onSubmit}
                        className="space-y-5"
                    >
                        <div className="space-y-2">
                            <Label htmlFor="email">
                                {t("auth.register.email")}
                            </Label>

                            <Input
                                id="email"
                                type="email"
                                required
                                autoComplete="email"
                                dir="ltr"
                                value={email}
                                onChange={(event) =>
                                    setEmail(event.target.value)
                                }
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="password">
                                {t("auth.register.password")}
                            </Label>

                            <Input
                                id="password"
                                type="password"
                                required
                                minLength={6}
                                autoComplete="new-password"
                                value={password}
                                onChange={(event) =>
                                    setPassword(event.target.value)
                                }
                            />
                        </div>

                        {error && (
                            <div
                                role="alert"
                                className="flex items-start gap-2 rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive"
                            >
                                <AlertCircle
                                    size={18}
                                    className="shrink-0"
                                />
                                <span>{error}</span>
                            </div>
                        )}

                        <button
                            type="submit"
                            disabled={submitting}
                            className="flex w-full items-center justify-center gap-2 rounded-xl bg-primary px-4 py-3 font-medium text-primary-foreground disabled:opacity-60"
                        >
                            {submitting && (
                                <Loader2
                                    size={18}
                                    className="animate-spin"
                                />
                            )}

                            {submitting
                                ? t("auth.register.submitting")
                                : t("auth.register.submit")}
                        </button>

                        <p className="!mt-10 text-center text-sm text-muted-foreground">
                            {t("auth.register.alreadyRegistered")}{" "}
                            <Link
                                to="/login"
                                className="font-medium text-primary hover:underline"
                            >
                                {t("auth.register.login")}
                            </Link>
                        </p>
                    </form>
                )}
            </div>
        </AuthLayout>
    );
}
