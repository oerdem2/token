﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using amorphie.token.data;

#nullable disable

namespace amorphie.token.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20240206220351_LogonEntity")]
    partial class LogonEntity
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("amorphie.token.core.Models.Token.Logon", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Error")
                        .HasColumnType("text");

                    b.Property<long>("LastJobKey")
                        .HasColumnType("bigint");

                    b.Property<int>("LogonStatus")
                        .HasColumnType("integer");

                    b.Property<int>("LogonType")
                        .HasColumnType("integer");

                    b.Property<string>("Reference")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<long>("WorkflowInstanceId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("Reference");

                    b.HasIndex("WorkflowInstanceId");

                    b.ToTable("Logon");
                });

            modelBuilder.Entity("amorphie.token.core.Models.Token.TokenInfo", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ClientId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid?>("ConsentId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("ExpiredAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("IsActive")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("IssuedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Reference")
                        .HasColumnType("text");

                    b.Property<Guid?>("RelatedTokenId")
                        .HasColumnType("uuid");

                    b.Property<List<string>>("Scopes")
                        .IsRequired()
                        .HasColumnType("text[]");

                    b.Property<int>("TokenType")
                        .HasColumnType("integer");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ConsentId");

                    b.HasIndex("Reference");

                    b.HasIndex("UserId");

                    b.ToTable("Tokens");
                });
#pragma warning restore 612, 618
        }
    }
}
