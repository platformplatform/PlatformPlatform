import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  Avatar,
  AvatarBadge,
  AvatarFallback,
  AvatarGroup,
  AvatarGroupCount,
  AvatarImage
} from "@repo/ui/components/Avatar";
import { CheckIcon } from "lucide-react";

export function AvatarPreview() {
  return (
    <>
      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Avatar sizes</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-4">
          <Avatar size="sm">
            <AvatarImage
              src="https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=96&h=96&fit=crop&crop=faces"
              alt={t`Alex Taylor`}
            />
            <AvatarFallback className="bg-sky-600 text-white">AT</AvatarFallback>
          </Avatar>
          <Avatar>
            <AvatarImage
              src="https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=96&h=96&fit=crop&crop=faces"
              alt={t`Jordan Lee`}
            />
            <AvatarFallback className="bg-rose-600 text-white">JL</AvatarFallback>
          </Avatar>
          <Avatar size="lg">
            <AvatarImage
              src="https://images.unsplash.com/photo-1599566150163-29194dcaad36?w=96&h=96&fit=crop&crop=faces"
              alt={t`Morgan Riley`}
            />
            <AvatarFallback className="bg-emerald-600 text-white">MR</AvatarFallback>
          </Avatar>
          <Avatar size="xl">
            <AvatarImage
              src="https://images.unsplash.com/photo-1527980965255-d3b416303d12?w=96&h=96&fit=crop&crop=faces"
              alt={t`Casey Park`}
            />
            <AvatarFallback className="bg-violet-600 text-white">CP</AvatarFallback>
          </Avatar>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Avatar fallback and badge</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-4">
          <Avatar>
            <AvatarFallback className="bg-amber-600 text-white">SB</AvatarFallback>
          </Avatar>
          <Avatar>
            <AvatarFallback className="bg-violet-600 text-white">KP</AvatarFallback>
            <AvatarBadge />
          </Avatar>
          <Avatar size="lg">
            <AvatarImage
              src="https://images.unsplash.com/photo-1527980965255-d3b416303d12?w=96&h=96&fit=crop&crop=faces"
              alt={t`Casey Park`}
            />
            <AvatarFallback className="bg-teal-600 text-white">CP</AvatarFallback>
            <AvatarBadge>
              <CheckIcon />
            </AvatarBadge>
          </Avatar>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Avatar group</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-4">
          <AvatarGroup>
            <Avatar>
              <AvatarFallback className="bg-sky-600 text-white">AT</AvatarFallback>
            </Avatar>
            <Avatar>
              <AvatarFallback className="bg-rose-600 text-white">JL</AvatarFallback>
            </Avatar>
            <Avatar>
              <AvatarFallback className="bg-emerald-600 text-white">MR</AvatarFallback>
            </Avatar>
            <AvatarGroupCount>+5</AvatarGroupCount>
          </AvatarGroup>
        </div>
      </div>
    </>
  );
}
