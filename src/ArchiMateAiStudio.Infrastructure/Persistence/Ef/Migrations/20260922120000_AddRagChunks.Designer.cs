using System;
using ArchiMateAiStudio.Infrastructure.Persistence.Ef;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ArchiMateAiStudio.Infrastructure.Persistence.Ef.Migrations
{
    [DbContext(typeof(StudioDbContext))]
    [Migration("20260922120000_AddRagChunks")]
    partial class AddRagChunks
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("ArchiMateAiStudio.Infrastructure.Persistence.Ef.ModelEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ArchimateXml")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("UpdatedAt");

                    b.ToTable("models", (string)null);
                });

            modelBuilder.Entity("ArchiMateAiStudio.Infrastructure.Persistence.Ef.ProposalEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid>("ModelId")
                        .HasColumnType("uuid");

                    b.Property<string>("PatchJson")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt");

                    b.HasIndex("ModelId");

                    b.ToTable("proposals", (string)null);
                });

            modelBuilder.Entity("ArchiMateAiStudio.Infrastructure.Persistence.Ef.RagChunkEntity", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("EmbeddingJson")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Kind")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)");

                    b.Property<string>("MetadataJson")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("ModelId")
                        .HasColumnType("uuid");

                    b.Property<string>("SourceId")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ModelId");

                    b.HasIndex("ModelId", "Kind", "SourceId")
                        .IsUnique();

                    b.ToTable("rag_chunks", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
