-- CreateTable
CREATE TABLE "public"."AdminMessage" (
    "id" SERIAL NOT NULL,
    "message" TEXT NOT NULL,
    "sent" BOOLEAN NOT NULL DEFAULT false,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "sentAt" TIMESTAMP(3),

    CONSTRAINT "AdminMessage_pkey" PRIMARY KEY ("id")
);
