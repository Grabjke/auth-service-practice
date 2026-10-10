import { authGet } from "../auth/authFetch";
import type { ApiResult, Envelope } from "./client";

// Ответ GET /api/users/me (UserProfileResponse на бэке)
export type UserProfile = {
  id: string;
  userName: string;
  email: string;
  emailConfirmed: boolean;
  phoneNumber: string | null;
  roles: string[];
  twoFactorEnabled: boolean;
};

export type GetUserResult = ApiResult & { profile: UserProfile | null };

export async function getUser(): Promise<GetUserResult> {
  const res = await authGet("/api/users/me");
  const profile =
    res.status === 200 ? (res.body as Envelope<UserProfile>).result : null;
  return { ...res, profile };
}
