import { Image, type ImageProps } from "expo-image";

const fallback = require("../../assets/images/testImage.png");

type MediaImageProps = Omit<ImageProps, "source" | "placeholder"> & {
  uri?: string | null;
};

export default function MediaImage({ uri, ...props }: MediaImageProps) {
  return (
    <Image
      {...props}
      source={uri ? { uri } : fallback}
      placeholder={fallback}
      placeholderContentFit="cover"
      contentFit="cover"
      transition={120}
    />
  );
}
