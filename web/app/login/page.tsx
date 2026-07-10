"use client";

import { signIn } from "next-auth/react";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { type FormEvent, useState } from "react";

export default function LoginPage() {
  const t = useTranslations("auth");
  const common = useTranslations("common");
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);
    setIsSubmitting(true);

    const result = await signIn("credentials", {
      email,
      password,
      redirect: false,
    });

    setIsSubmitting(false);

    if (result?.ok) {
      router.push("/");
      return;
    }

    setError(t("invalid"));
  }

  return (
    <main className="min-h-screen bg-[#0f172a] text-slate-100">
      <div className="mx-auto flex min-h-screen w-full max-w-6xl items-center justify-center px-6 py-12">
        <section className="grid w-full gap-8 lg:grid-cols-[1.1fr_0.9fr] lg:items-center">
          <div className="space-y-6">
            <div className="inline-flex items-center rounded-full border border-cyan-300/30 bg-cyan-300/10 px-3 py-1 text-sm font-medium text-cyan-100">
              {t("badge")}
            </div>
            <div className="space-y-4">
              <h1 className="text-4xl font-semibold tracking-tight sm:text-5xl">
                {common("brand")}
              </h1>
              <p className="max-w-xl text-lg leading-8 text-slate-300">
                {t("description")}
              </p>
            </div>
          </div>

          <form
            className="rounded-lg border border-slate-700 bg-slate-950/80 p-6 shadow-2xl shadow-cyan-950/30"
            onSubmit={handleSubmit}
          >
            <div className="space-y-2">
              <h2 className="text-2xl font-semibold text-white">{t("signIn")}</h2>
              <p className="text-sm text-slate-400">
                {t("hint")}
              </p>
            </div>

            <div className="mt-6 space-y-4">
              <label className="block text-sm font-medium text-slate-200">
                {t("email")}
                <input
                  className="mt-2 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 outline-none transition focus:border-cyan-300 focus:ring-2 focus:ring-cyan-300/20"
                  name="email"
                  onChange={(event) => setEmail(event.target.value)}
                  required
                  type="email"
                  value={email}
                />
              </label>

              <label className="block text-sm font-medium text-slate-200">
                {t("password")}
                <input
                  className="mt-2 w-full rounded-md border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 outline-none transition focus:border-cyan-300 focus:ring-2 focus:ring-cyan-300/20"
                  name="password"
                  onChange={(event) => setPassword(event.target.value)}
                  required
                  type="password"
                  value={password}
                />
              </label>
            </div>

            {error ? (
              <p className="mt-4 rounded-md border border-red-400/40 bg-red-500/10 px-3 py-2 text-sm text-red-100" role="alert">
                {error}
              </p>
            ) : null}

            <button
              className="mt-6 w-full rounded-md bg-cyan-300 px-4 py-2.5 text-sm font-semibold text-slate-950 transition hover:bg-cyan-200 disabled:cursor-not-allowed disabled:opacity-70"
              disabled={isSubmitting}
              type="submit"
            >
              {isSubmitting ? t("signingIn") : t("signIn")}
            </button>
          </form>
        </section>
      </div>
    </main>
  );
}
