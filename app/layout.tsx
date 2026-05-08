import type { Metadata } from 'next';
import './globals.css';

export const metadata: Metadata = {
  title: 'Flow Kiến Trúc AI Studio — Phú Cường Group',
  description: 'Studio local bán tự động tạo prompt Google Flow cho video kiến trúc.'
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="vi">
      <body>{children}</body>
    </html>
  );
}
