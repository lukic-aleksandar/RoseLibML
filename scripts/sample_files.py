import os
import random
import mmap
import shutil


def count_lines(filename):
    try:
        f = open(filename, "r+")
        buf = mmap.mmap(f.fileno(), 0)
        lines = 0
        while buf.readline():
            lines += 1
    except:
        print("Could not count lines for file:", filename)
        return float('inf')
    return lines


def sample(in_dir, out_dir, n):
    if not os.path.exists(out_dir):
        os.makedirs(out_dir)

    sampled_files = random.sample(os.listdir(in_dir), n)
    for file in sampled_files:
        full_path = os.path.join(in_dir, file)
        shutil.copyfile(full_path, os.path.join(out_dir, file))


def filter_files_up_to_n_lines_of_code(in_dir, out_dir, n):
    if not os.path.exists(out_dir):
        os.makedirs(out_dir)

    for file in os.listdir(in_dir):
        full_path = os.path.join(in_dir, file)

        if count_lines(full_path) <= n:
            shutil.copyfile(full_path, os.path.join(out_dir, file))


def sample_files_up_to_n_lines_of_code(in_dir, out_dir, n, sample_size):
    if not os.path.exists(out_dir):
        os.makedirs(out_dir)

    files = []
    for file in os.listdir(in_dir):
        full_path = os.path.join(in_dir, file)

        if count_lines(full_path) <= n:
            files.append((file, full_path))

    for file, file_path in random.sample(files, sample_size):
        shutil.copyfile(file_path, os.path.join(out_dir, file))

sample("C:\\Users\\nenad\\Desktop\\Nnd\\doktorske\\trainingsmall", "C:\\Users\\nenad\\Desktop\\Nnd\\doktorske\\training100small", 100)
            